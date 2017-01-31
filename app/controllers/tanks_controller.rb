class TanksController < ApplicationController

	def index
		#@q = Tank.joins(:marks).distinct.ransack(params[:q])
		#@tanks = @q.result.paginate(:page => params[:page], :per_page => 20)
		@q = Tank.distinct.ransack(params[:q])
		@tanks = @q.result.includes(:marks).paginate(:page => params[:page], :per_page => 20)
	end
	
	def show
		@tank = Tank.find(params[:id])
		#@q = @tank.ransack(params[:q])
		@marks = @tank.marks.paginate(:page => params[:page], :per_page => 20)
		rescue ActiveRecord::RecordNotFound
			redirect_to tanks_path and return
	end
end
