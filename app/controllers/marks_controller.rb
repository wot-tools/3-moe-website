class MarksController < ApplicationController

	def index
		@q = Mark.all.ransack(params[:q])
		@marks = @q.result.includes(:player, player: [:clan]).paginate(:page => params[:page], :per_page => 20)
	end

end
