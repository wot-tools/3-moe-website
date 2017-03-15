class VehicleTypesController < ApplicationController

	def index
		@q = VehicleType.distinct.ransack(params[:q])
		@vehicle_types = @q.result.paginate(:page => params[:page], :per_page => 20)
	end
	
	def show
		@vehicle_type = VehicleType.find(params[:id])
		
		@q = @vehicle_type.tanks.ransack(params[:q])
		@tanks = @q.result.includes(:nation).paginate(:page => params[:page], :per_page => 20)
		
		rescue ActiveRecord::RecordNotFound
			redirect_to vehicle_types_path and return
	end

end
